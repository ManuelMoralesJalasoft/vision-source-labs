using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web.App.Models;
using Web.App.Repositories;

namespace Web.App.Pages;

internal sealed class IndexModel : PageModel
{
    private readonly IToDosRepository _toDosRepository;
    public IEnumerable<ToDo> ToDos { get; set; } = [];

    public IndexModel(IToDosRepository toDosRepository)
    {
        _toDosRepository = toDosRepository;
    }

    public async Task OnGetAsync()
    {
        ToDos = await _toDosRepository.GetAllAsync();
    }

    public async Task<IActionResult> OnPostAsync(string title, bool isCompleted)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("Title", "Title is required.");

            return Page();
        }

        var newToDo = new ToDo { Title = title.Trim(), IsCompleted = isCompleted };

        await _toDosRepository.AddAsync(newToDo);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _toDosRepository.DeleteAsync(id);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id)
    {
        var todo = await _toDosRepository.GetByIdAsync(id);

        if (todo is null)
        {
            return NotFound();
        }

        todo.IsCompleted = !todo.IsCompleted;

        await _toDosRepository.ToggleStatusAsync(todo.Id);

        return RedirectToPage();
    }
}
