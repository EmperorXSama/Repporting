namespace RepportingApp.Helper;

public static class ConsoleWriter
{
  public static void WriteListLine<T>(this List<T> items, string fileName = "Items.txt")
{
    try
    {
        if (items == null || !items.Any())
        {
            Console.WriteLine("No items to write to the file.");
            return;
        }

        // Get the desktop path
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath,"check", fileName);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            foreach (var item in items)
            {
                if (item == null)
                {
                    writer.WriteLine("Null item");
                    continue;
                }

                if (item is EmailAccount emailAccount)
                {
                    // Special handling for EmailAccount
                    writer.WriteLine($"Proxy IP: {emailAccount.Proxy?.ProxyIp ?? "No Proxy"}");
                }
                else
                {
                    // Generic handling for other types
                    var properties = item.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item) ?? "null";
                        writer.WriteLine($"{prop.Name}: {value}");
                    }
                    writer.WriteLine("-------------------------------------------"); // Separator for items
                }
            }
            
            writer.WriteLine(); // Add an extra line for readability
        }

        Console.WriteLine($"Items written to file: {filePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to write items to file. Error: {ex.Message}");
    }
}


}