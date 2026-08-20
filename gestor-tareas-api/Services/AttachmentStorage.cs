using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace GestorTareas.Api.Services;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}

public sealed class LocalAttachmentStorage(IWebHostEnvironment environment) : IAttachmentStorage
{
    private readonly string _root = Path.Combine(environment.ContentRootPath, "uploads");

    public async Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relative = Path.Combine(taskId.ToString("N"), $"{Guid.NewGuid():N}{extension}");
        var fullPath = SafePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = File.Create(fullPath);
        await file.CopyToAsync(output, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        var path = SafePath(relativePath);
        Stream? stream = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        var path = SafePath(relativePath);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string SafePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var root = Path.GetFullPath(_root) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ruta de archivo inválida.");
        return fullPath;
    }
}

public sealed class S3StorageOptions
{
    public string Provider { get; set; } = "Local";
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Bucket { get; set; } = "adjuntos";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public sealed class S3AttachmentStorage : IAttachmentStorage, IDisposable
{
    private readonly S3StorageOptions _options;
    private readonly AmazonS3Client _client;

    public S3AttachmentStorage(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.AccessKey) ||
            string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(_options.Region))
            throw new InvalidOperationException("La configuración S3 está incompleta.");
        _client = new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), new AmazonS3Config
        {
            ServiceURL = _options.Endpoint,
            AuthenticationRegion = _options.Region,
            ForcePathStyle = true
        });
    }

    public async Task<string> SaveAsync(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key = $"{taskId:N}/{Guid.NewGuid():N}{extension}";
        await using var input = file.OpenReadStream();
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = input,
            ContentType = file.ContentType,
            AutoCloseStream = false
        }, cancellationToken);
        return key;
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(_options.Bucket, relativePath, cancellationToken);
            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            return memory;
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) =>
        _client.DeleteObjectAsync(_options.Bucket, relativePath, cancellationToken);

    public void Dispose() => _client.Dispose();
}
