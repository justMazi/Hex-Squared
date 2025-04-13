using System.Text.Json;

namespace Domain.Players.MCTS;


public static class TrainingDataStorage
{
    private static readonly List<TrainingSample> Samples = new();
    private static readonly string FolderPath = "C:\\training_data"; // Folder for storing JSON files

    static TrainingDataStorage()
    {
        // Ensure directory exists
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }
    }

    // Add new training sample
    public static void AddTrainingSample(TrainingSample sample)
    {
        Samples.Add(sample);
    }

    // Flush all stored samples to a new file
    public static void FlushToDisk(int winner)
    {
        if (Samples.Count == 0) return; // No samples to flush


        
        foreach (var sample in Samples)
        {
            // draw
            if (winner == -1)
            {
                sample.RootValue = 0;
            }
            else
            {
                sample.RootValue = (sample.IsRotated ? -1 : 1) * (sample.CurrentPlayer == winner ? 1 : -1);
            }
        }
        
        try
        {
            // Generate unique filename using timestamp
            var fileName = $"training_batch_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FolderPath, fileName);

            // Serialize and write to file
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonData = JsonSerializer.Serialize(Samples, jsonOptions);
            File.WriteAllText(filePath, jsonData);

            Console.WriteLine($"Flushed {Samples.Count} samples to {filePath}");

            Samples.Clear(); // Clear memory after saving
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while flushing training data: {ex.Message}");
        }
    }
}


