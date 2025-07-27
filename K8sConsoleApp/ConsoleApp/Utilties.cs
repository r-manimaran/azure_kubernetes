using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp;

public static class Utilities
{
    public static string GetContainerName(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
        {
            throw new ArgumentException("Blob URL cannot be null or empty.", nameof(blobUrl));
        }
        var uri = new Uri(blobUrl);
        var segments = uri.Segments;

        // Assuming the container name is the first segment after the host
        if (segments.Length < 2)
        {
            throw new ArgumentException("Invalid blob URL format.", nameof(blobUrl));
        }
        return segments[1].TrimEnd('/');
    }

    public static string GetBlobFileName(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
        {
            throw new ArgumentException("Blob URL cannot be null or empty.", nameof(blobUrl));
        }
        var uri = new Uri(blobUrl);
        var segments = uri.Segments;

        // Assuming the blob file name is the last segment
        if (segments.Length == 0)
        {
            throw new ArgumentException("Invalid blob URL format.", nameof(blobUrl));
        }
        return segments.Last().TrimEnd('/');
    }

    public static string GetBlobFileExtension(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
        {
            throw new ArgumentException("Blob URL cannot be null or empty.", nameof(blobUrl));
        }
        var fileName = GetBlobFileName(blobUrl);
        var lastDotIndex = fileName.LastIndexOf('.');

        // If there is no dot, return an empty string
        if (lastDotIndex < 0 || lastDotIndex == fileName.Length - 1)
        {
            return string.Empty;
        }

        // Return the file extension including the dot
        return fileName.Substring(lastDotIndex);
    }
}