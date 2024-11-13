using System.Diagnostics;

public class VehiclePosition
{
    public int VehicleId { get; set; }
    public required string Registration { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
}

public class KdTreeNode
{
    public required VehiclePosition Vehicle { get; set; }
    public required KdTreeNode Left { get; set; }
    public required KdTreeNode Right { get; set; }
}

public class KdTree
{
    public KdTreeNode? Root { get; private set; }

    /// <summary>
    /// Builds the Kd-Tree from a list of vehicle positions.
    /// Splits data at the median on alternating axes (Latitude and Longitude) at each depth level.
    /// </summary>
    /// <param name="vehicles">List of vehicle positions to build the tree.</param>
    /// <param name="depth">Current depth in the tree (to determine the split axis).</param>
    public void Build(List<VehiclePosition> vehicles, int depth = 0)
    {
        if (!vehicles.Any()) return;

        // Convert List to array for efficient index manipulation
        var vehicleArray = vehicles.ToArray();

        // Start building the tree from root
        Root = BuildSubtree(vehicleArray, 0, vehicleArray.Length - 1, depth);
    }

    /// <summary>
    /// Recursively builds the Kd-Tree by selecting median elements.
    /// Median element is chosen as root, and the array is partitioned for left and right subtrees.
    /// </summary>
    private KdTreeNode BuildSubtree(VehiclePosition[] vehicles, int left, int right, int depth)
    {
        if (left > right) return null;

        int axis = depth % 2;  // Split axis: 0 for Latitude, 1 for Longitude

        // Find median element's index using Quickselect
        int medianIndex = Quickselect(vehicles, left, right, (left + right) / 2, axis);

        // Create a tree node with the median vehicle
        var node = new KdTreeNode
        {
            Vehicle = vehicles[medianIndex],

            // Recursively build left and right subtrees
            Left = BuildSubtree(vehicles, left, medianIndex - 1, depth + 1),
            Right = BuildSubtree(vehicles, medianIndex + 1, right, depth + 1),
        };

        return node;
    }

    /// <summary>
    /// Quickselect algorithm to efficiently find median element without full sorting.
    /// Splits data based on chosen axis (latitude or longitude).
    /// </summary>
    private int Quickselect(VehiclePosition[] arr, int left, int right, int k, int axis)
    {
        if (left == right) return left;

        int pivotIndex = Partition(arr, left, right, axis);

        // Recur based on position of pivot relative to 'k'
        if (k == pivotIndex)
            return k;
        else if (k < pivotIndex)
            return Quickselect(arr, left, pivotIndex - 1, k, axis);
        else
            return Quickselect(arr, pivotIndex + 1, right, k, axis);
    }

    /// <summary>
    /// Partitions array around a pivot element for Quickselect.
    /// Elements are ordered based on axis (latitude or longitude).
    /// </summary>
    private int Partition(VehiclePosition[] arr, int left, int right, int axis)
    {
        VehiclePosition pivot = arr[right];
        int i = left;

        // Partitioning by comparing each element with pivot
        for (int j = left; j < right; j++)
        {
            bool condition = (axis == 0) ? arr[j].Latitude < pivot.Latitude : arr[j].Longitude < pivot.Longitude;
            if (condition)
            {
                Swap(arr, i, j);
                i++;
            }
        }

        Swap(arr, i, right);  // Place pivot at correct position
        return i;
    }

    /// <summary>
    /// Swaps two elements in the array.
    /// </summary>
    private void Swap(VehiclePosition[] arr, int i, int j)
    {
        VehiclePosition temp = arr[i];
        arr[i] = arr[j];
        arr[j] = temp;
    }

    /// <summary>
    /// Finds the nearest vehicle to the target coordinates.
    /// </summary>
    public VehiclePosition? FindNearest(float targetLat, float targetLon)
    {
        return Root != null ? FindNearest(Root, targetLat, targetLon) : null;
    }

    /// <summary>
    /// Recursively searches for the nearest vehicle using k-d tree properties.
    /// Compares current node, chosen subtree, and opposite subtree if necessary.
    /// </summary>
    private VehiclePosition? FindNearest(KdTreeNode node, float targetLat, float targetLon, int depth = 0)
    {
        if (node == null) return null;

        int axis = depth % 2;
        KdTreeNode nextNode = (axis == 0 && targetLat < node.Vehicle.Latitude) || (axis == 1 && targetLon < node.Vehicle.Longitude)
            ? node.Left : node.Right;
        KdTreeNode oppositeNode = nextNode == node.Left ? node.Right : node.Left;

        // Recursive search in subtree
        var nextBest = FindNearest(nextNode, targetLat, targetLon, depth + 1);
        var best = nextBest != null ? CloserDistance(targetLat, targetLon, node.Vehicle, nextBest) : node.Vehicle;

        // Check opposite subtree if closer node might exist across the split
        if (Distance(targetLat, targetLon, axis == 0 ? node.Vehicle.Latitude : targetLat, axis == 1 ? node.Vehicle.Longitude : targetLon)
            < Distance(targetLat, targetLon, best.Latitude, best.Longitude))
        {
            var oppositeBest = FindNearest(oppositeNode, targetLat, targetLon, depth + 1);
            best = CloserDistance(targetLat, targetLon, best, oppositeBest ?? node.Vehicle);
        }

        return best;
    }

    /// <summary>
    /// Determines which of two vehicles is closer to a target location.
    /// </summary>
    private VehiclePosition CloserDistance(float lat, float lon, VehiclePosition p1, VehiclePosition p2)
    {
        if (p1 == null) return p2;
        if (p2 == null) return p1;
        return Distance(lat, lon, p1.Latitude, p1.Longitude) < Distance(lat, lon, p2.Latitude, p2.Longitude) ? p1 : p2;
    }

    /// <summary>
    /// Calculates Euclidean distance between two points.
    /// </summary>
    private double Distance(float lat1, float lon1, float lat2, float lon2)
    {
        return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
    }
}

public class Program
{
    private static readonly (float Latitude, float Longitude)[] Targets = {
        (34.544909f, -102.10084f),
        (32.345544f, -99.123124f),
        (33.234235f, -100.21412f),
        (35.195739f, -95.348899f),
        (31.895839f, -97.789573f),
        (32.895839f, -101.78957f),
        (34.115839f, -100.22573f),
        (32.335839f, -99.992232f),
        (33.535339f, -94.792232f),
        (32.234235f, -100.22222f)
    };

    public static void Main()
    {
        string filePath = "VehiclePositions.dat";

        // Timing data loading
        var loadTimer = Stopwatch.StartNew();
        var vehiclePositions = LoadVehicleData(filePath);
        loadTimer.Stop();
        Console.WriteLine($"Data loading time: {loadTimer.ElapsedMilliseconds} ms");

        // Timing k-d tree build
        var kdTree = new KdTree();
        var buildTimer = Stopwatch.StartNew();
        kdTree.Build(vehiclePositions);
        buildTimer.Stop();
        Console.WriteLine($"k-d tree build time: {buildTimer.ElapsedMilliseconds} ms");

        // Timing nearest search for each target
        var searchTimer = Stopwatch.StartNew();
        foreach (var (lat, lon) in Targets)
        {
            var nearestVehicle = kdTree.FindNearest(lat, lon);
            if (nearestVehicle != null)
            {
                Console.WriteLine($"Nearest vehicle to ({lat}, {lon}): ID {nearestVehicle.VehicleId}, Reg {nearestVehicle.Registration}, at ({nearestVehicle.Latitude}, {nearestVehicle.Longitude})");
            }
        }
        searchTimer.Stop();
        Console.WriteLine($"Total nearest search time: {searchTimer.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Loads vehicle data from a binary file, skipping unnecessary data to optimize memory usage.
    /// </summary>
    public static List<VehiclePosition> LoadVehicleData(string filePath)
    {
        var vehicles = new List<VehiclePosition>(2000000);  // Preset for 2 million records

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8192, useAsync: true))
        using (BinaryReader reader = new BinaryReader(fs, encoding: System.Text.Encoding.UTF8, leaveOpen: false))
        {
            long fileLength = fs.Length;

            while (fs.Position < fileLength)
            {
                int vehicleId = reader.ReadInt32();
                string registration = ReadNullTerminatedString(reader);
                float latitude = reader.ReadSingle();
                float longitude = reader.ReadSingle();
                reader.ReadUInt64();  // Skip RecordedTimeUTC

                vehicles.Add(new VehiclePosition
                {
                    VehicleId = vehicleId,
                    Registration = registration,
                    Latitude = latitude,
                    Longitude = longitude
                });
            }
        }

        return vehicles;
    }

    /// <summary>
    /// Reads a null-terminated string from the BinaryReader for better performance.
    /// </summary>
    static string ReadNullTerminatedString(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte b;
        while ((b = reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }
        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
}
