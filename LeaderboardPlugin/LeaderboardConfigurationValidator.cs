using FluentValidation;

namespace LeaderboardPlugin;

public class LeaderboardConfigurationValidator : AbstractValidator<LeaderboardConfiguration>
{
    public LeaderboardConfigurationValidator()
    {
        RuleFor(cfg => cfg.ApiUrl).NotEmpty().Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ApiUrl must be a valid absolute URL");
        RuleFor(cfg => cfg.ApiKey).NotEmpty()
            .WithMessage("ApiKey is required — generate one in the leaderboard admin");
        RuleFor(cfg => cfg.MinLapTimeSec).GreaterThan((uint)0);
    }
}
