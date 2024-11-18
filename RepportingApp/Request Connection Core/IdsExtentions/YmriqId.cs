namespace RepportingApp.Request_Connection_Core.IdsExtentions;

public static class YmriqId
{
    private static string GenerateNextId2(string currentId)
    {
        var parts = currentId.Split('-');

        // Get the last part of the ID
        var lastPart = parts[4];

        // Split the last part into its constituent parts
        var prefix = lastPart.Substring(0, 4);
        var numericPart = lastPart.Substring(4, 2);
        var postfix = lastPart.Substring(6);

        // Convert the numeric part to an integer and increment it
        var intValue = Convert.ToInt32(numericPart, 16);
        intValue++;

        var newNumericPart = intValue.ToString("x2");

        // Recombine the parts
        parts[4] = prefix + newNumericPart + postfix;

        return string.Join('-', parts);
    }
    //string currentID = "35cabb3e-18eb-8c7e-1ce6-b20003019600";

    public static List<string> GetYmreqid(string current, int number)
    {
        var nextId = current ?? throw new ArgumentNullException(nameof(current));
        var currentId = new List<string>();
        for (var i = 0; i < number; i++)
        {
            nextId = GenerateNextId2(nextId);
            currentId.Add(nextId);
        }

        return currentId;
    }
}