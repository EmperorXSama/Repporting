namespace RepportingApp.CoreSystem.Multithread;

public class SystemConfigurationEstimator
{
    public int LogicalProcessorCount { get; }
    public int RecommendedMaxDegreeOfParallelism { get; private set; }
    public int RecommendedBatchSize { get; private set; }

    public SystemConfigurationEstimator()
    {
        LogicalProcessorCount = Environment.ProcessorCount;
        EstimateOptimalSettings();
    }

    private void EstimateOptimalSettings()
    {
        // Increase maxDegreeOfParallelism for high-performance CPUs
        if (LogicalProcessorCount <= 8)
        {
            RecommendedMaxDegreeOfParallelism = LogicalProcessorCount / 2;
        }
        else if (LogicalProcessorCount <= 16)
        {
            RecommendedMaxDegreeOfParallelism = (int)(LogicalProcessorCount * 0.75);
        }
        else
        {
            RecommendedMaxDegreeOfParallelism = (int)(LogicalProcessorCount * 0.85);
        }

        // Scale batch size dynamically based on logical processors
        RecommendedBatchSize = LogicalProcessorCount * 5;
        
        
        if (RecommendedBatchSize > 200)
        {
            RecommendedBatchSize = 200; // Cap to avoid excessive load
        }
    }

}