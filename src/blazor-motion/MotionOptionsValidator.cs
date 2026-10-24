using Microsoft.Extensions.Options;

namespace BlazorMotion;

/// <summary>
/// Rejects <see cref="MotionOptions"/> that would fail at runtime: an empty module path or a negative
/// default duration or delay. Every violated rule is reported, not just the first one.
/// </summary>
public sealed class MotionOptionsValidator : IValidateOptions<MotionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MotionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ModulePath))
        {
            failures.Add($"{nameof(MotionOptions)}.{nameof(MotionOptions.ModulePath)} must not be null or whitespace.");
        }

        if (options.DefaultTransition is not { } transition)
        {
            failures.Add($"{nameof(MotionOptions)}.{nameof(MotionOptions.DefaultTransition)} must not be null.");
        }
        else
        {
            if (transition.Duration < TimeSpan.Zero)
            {
                failures.Add($"{nameof(MotionOptions)}.{nameof(MotionOptions.DefaultTransition)}.{nameof(MotionTransition.Duration)} must not be negative; got {transition.Duration}.");
            }

            if (transition.Delay < TimeSpan.Zero)
            {
                failures.Add($"{nameof(MotionOptions)}.{nameof(MotionOptions.DefaultTransition)}.{nameof(MotionTransition.Delay)} must not be negative; got {transition.Delay}.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
