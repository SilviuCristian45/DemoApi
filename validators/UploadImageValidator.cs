// Validators/UploadImageValidator.cs
using FluentValidation;
using DemoApi.Models;

public class UploadImageValidator : AbstractValidator<UploadImageDto>
{
    public UploadImageValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Fișierul este obligatoriu!")
            // Verificare mărime (2MB)
            .Must(file => file.Length <= 2 * 1024 * 1024)
            .When(x => x.File != null) // Rulăm doar dacă nu e null
            .WithMessage("Fișierul este prea mare! Maxim 2MB.");

        RuleFor(x => x.File)
            // Verificare extensie
            .Must(file => 
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(file.FileName).ToLower();
                return allowedExtensions.Contains(ext);
            })
            .When(x => x.File != null)
            .WithMessage("Doar formatele .jpg, .jpeg și .png sunt permise!");
    }
}