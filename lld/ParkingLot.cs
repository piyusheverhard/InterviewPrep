/*

Problem 2: The Parking Lot
The Requirements:

The parking lot has multiple levels (floors).

The parking lot has multiple parking spot sizes: Motorcycle, Compact, and Large.

It needs to support different vehicle types: Motorcycle, Car, and Truck.

The Parking Rule:

A Motorcycle can park in any spot.

A Car can park in a Compact or Large spot.

A Truck can only park in a Large spot.

When a vehicle enters, the system must assign it the nearest available parking spot, mark it as occupied, and issue a ParkingTicket.

When a vehicle exits, the system must calculate the fee based on the time parked, process the payment, and free up the spot.

*/

public enum ParkingSpotType
{
    MotorCycle,
    Compact,
    Large
}

public enum VehicleType
{
    MotorCycle,
    Car,
    Truck
}

public class ParkingSpot {
    public ParkingSpotType SpotType { get; init; }
    public Vehicle? Vehicle { get; private set; }
    public bool IsOccupied() => Vehicle != null;
    public bool ParkVehicle(Vehicle vehicle)
    {
        if (IsOccupied())
        {
            return false;
        }
        Vehicle = vehicle;
        return true;
    }

    public void Clear()
    {
        Vehicle = null;
    }
}

public interface IFloor {
    public ParkingSpot? GetNearestCompatibleParkingSpot(VehicleType vehicleType);
}

public class OneDimensionalFloor : IFloor
{
    private readonly int _numberOfSpots;
    private readonly int _numMotorCycleSpot;
    private readonly int _numCompactSpot;
    private readonly int _numLargeSpot;
    private ParkingSpot[] _parkingSpots; // Assuming we first have smaller spots then larger ones.

    public OneDimensionalFloor(int numMotorCycleSpot, int numCompactSpot, int numLargeSpot)
    {
        _numberOfSpots = numMotorCycleSpot + numCompactSpot + numLargeSpot;
        _numMotorCycleSpot = numMotorCycleSpot;
        _numCompactSpot = numCompactSpot;
        _numLargeSpot = numLargeSpot;
        _parkingSpots = new ParkingSpot[_numberOfSpots];
        for (int i = 0; i < numMotorCycleSpot; i++)
        {
            _parkingSpots[i] = new ParkingSpot(ParkingSpotType.MotorCycle);
        }
        for (int i = 0; i < numCompactSpot; i++)
        {
            _parkingSpots[i] = new ParkingSpot(ParkingSpotType.Compact);
        }
        for (int i = 0; i < numLargeSpot; i++)
        {
            _parkingSpots[i] = new ParkingSpot(ParkingSpotType.Large);
        }
    }

    public ParkingSpot? GetNearestCompatibleParkingSpot(VehicleType vehicleType)
    {
        for (int i = 0; i < _numberOfSpots; i++)
        {
            if (!_parkingSpots[i].IsOccupied() && IsCompatible(vehicleType, _parkingSpots[i].SpotType))
            {
                return _parkingSpot[i];
            }
        }
    }

    private bool IsCompatibleVehicleForParkingSpot(VehicleType vehicleType, ParkingSpotType parkingSpotType)
    {
        return (int)vehicleType <= (int)parkingSpotType;
    }
}

public class ParkingLot {
    private readonly int _numberOfFloors;
    private IFloor[] _floors;
    private IFeeCalculator _feeCalculator;

    private Dictionary<string, ParkingSpot> _vehicleParkingSpotRecord;

    private ParkingLot(int numberOfFloors, IFloor[] floors, IFeeCalculator feeCalculator)
    {
        _numberOfFloors = numberOfFloors;
        _floors = floors;
        _feeCalculator = feeCalculator;
        _vehicleParkingSpotRecord = [];
    }

    public bool Enter(string vehicleNumber, VehicleType vehicleType)
    {
        foreach (var floor in _floors)
        {
            var parkingSpot = floor.GetNearestCompatibleParkingSpot(vehicleType);
            if (parkingSpot != null)
            {
                parkingSpot.ParkVehicle(new Vehicle(vehicleNumber, vehicleType));
                var added = _vehicleParkingSpotRecord.TryAdd(vehicleNumber, parkingSpot);
                if (added)
                {
                    return true;
                }
                else
                {
                    throw new InvalidOperationException($"vehicle already parked: {vehicleNumber}");
                }
            }
        }
        return false;
    }

    public double ExitVehicleAndGetFee(string vehicleNumber) {
        _vehicleParkingSpotRecord.TryGetValue(vehicleNumber, out var parkingSpot);
        if (parkingSpot == null) {
            throw new InvalidOperationException($"vehicle is not parked: {vehicleNumber}");
        }
        var vehicle = parkingSpot.Vehicle;
        parkingSpot.Clear();

    }

    public class ParkingLotBuilder {

        private List<IFloor> _floors;

        public ParkingLotBuilder WithOneDimensionalFloor(int numMotorcycleSpots, int numCompactSpots, int numLargeSpots)
        {
            _numberOfFloors++;
            _floors.Add(new OneDimensionalFloor(numMotorcycleSpots, numCompactSpots, numLargeSpots));
            return this;
        }

        public ParkingLot Build() {
            if (_floors.Count == 0) {
                throw InvalidOperationException("Please Add atleast one floor.");
            }
            IFeeCalculator feeCalculator = new DurationFeeCalculator(new BaseFeeCalculator());
            return new ParkingLot(_floors.Count, [.. _floors], feeCalculator);
        }
    }
}

public interface IFeeCalculator {
    public double CalculateFee(Vehicle vehicle);
}

public class DurationFeeCalculator : IFeeCalculator {
    private readonly IFeeCalculator? _inner;

    public DurationFeeCalculator(IFeeCalculator? innerCalculator)
    {
        _inner = innerCalculator;
    }

    public double CalculateFee(Vehicle vehicle) {
        var fee = _inner.CalculateFee(vehicle);
        var durationInHours = (DateTime.UtcNow - vehicle.EntryTime).TotalHours;
        fee += durationInHours * GetFeePerHourForVehicleType(vehicle.VehicleType);
        return fee;
    }

    private double GetFeePerHourForVehicleType(VehicleType type)
    {
        return type switch
        {
            VehicleType.MotorCycle => 10.0,
            VehicleType.Car => 20.0,
            VehicleType.Truck => 40.0,
            _ => throw new ArgumentOutOfRangeException(type),
        };
    }
}

public class BaseFeeCalculator : IFeeCalculator {
    public double CalculateFee(Vehicle vehicle) => GetBaseFeeForVehicleType(vehicle.VehicleType);

    private double GetBaseFeeForVehicleType(VehicleType type)
    {
        return type switch
        {
            VehicleType.MotorCycle => 10.0,
            VehicleType.Car => 20.0,
            VehicleType.Truck => 40.0,
            _ => throw new ArgumentOutOfRangeException(type),
        };
    }
}

public class Vehicle
{
    public VehicleType VehicleType { get; init; }
    public string VehicleNumber { get; init; }
    public DateTime EntryTime { get; init; }

    public Vehicle(VehicleType vehicleType, string vehicleNumber)
    {
        VehicleType = vehicleType;
        VehicleNumber = vehicleNumber;
        EntryTime = DateTime.UtcNow;
    }
}
