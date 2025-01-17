using WebApi.Dtos;
using WebApi.Models;

namespace WebApi.Services;

public interface IPostService
{
    Task<IEnumerable<PostResponse>> GetAllAsync();
    Task<PostResponse> GetByIdAsync(Guid id);
    Task<PostResponse> CreateAsync(CreatePostRequest post);
    Task<PostResponse> UpdateAsync(UpdatePostRequest post);
    Task DeleteAsync(Guid id);
}
