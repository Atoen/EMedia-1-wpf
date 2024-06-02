namespace Emedia_1_wpf.Extensions;

public static class ProgressReporter
{
    public static IEnumerable<TResult> Select<TSource, TResult>(
        this IEnumerable<TSource> source,
        double progressStep,
        IProgress<double>? progress,
        Func<TSource, TResult> selector,
        double updateThreshold = 0.005)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        
        var lastReportedProgress = 0.0;

        return source.Select((x, i) =>
        {
            var totalProgress = (i + 1) * progressStep;
            if (totalProgress - lastReportedProgress >= updateThreshold)
            {
                lastReportedProgress = totalProgress;
                progress?.Report(totalProgress);
            }
            
            return selector(x);
        });
    }
}
