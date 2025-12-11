using System.Threading.Tasks;

namespace Ginger.Services;

public interface IFileService
{
    Task<string?> OpenFileAsync(string title, string[] filters);
    Task<string?> SaveFileAsync(string title, string defaultName, string[] filters);
    Task<string?> SelectFolderAsync(string title);
}
