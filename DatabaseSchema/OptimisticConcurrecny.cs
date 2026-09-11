/*
 The Scenario:
  We are building the backend for Uber/Lyft. A rider requests a ride, and a notification pings 5     
  nearby drivers.
  At the exact same millisecond, Driver A, Driver B, and Driver C all tap "Accept Ride".             
  
  Only one driver can be assigned the ride. If we aren't careful, Driver A could get the success     
  screen, but Driver C's subsequent update silently overwrites Driver A, leaving Driver A driving to 
  a passenger who doesn't expect them!
  
  Your Task:
  Write the code for this scenario using Entity Framework Core (or general C# principles). You must  
  write:
  
  1. The Ride Entity (Show the properties required to enforce Optimistic Concurrency).
  2. The AcceptRideAsync(Guid rideId, Guid driverId) method in your Application Service.             
      • Show how you update the entity.
      • Show how you handle the exact moment a concurrency collision happens (i.e., when Driver B and
      C try to accept the ride a millisecond after Driver A).
      • What should this method return to the client?
*/

namespace OptimisticConcurrency;

public class Ride
{
    public Guid RideId { get; init; }
    public Guid UserId { get; init; }
    public Guid? DriverId { get; private set; }
    public Location PickupLocation { get; set; }
    public Location DropLocation { get; set; }

    public Ride(Guid userId, Location pickup, Location drop)
    {
        RideId = new Guid();
        PickupLocation = pickup;
        DropLocation = drop;
    }

    public void AssignDriver(Guid driverId)
    {
        DriverId = driverId;
    }

    public void UpdateDriver(Guid driverId) => DriverId = driverId;
}

public record Location
{
    public double Longitude;
    public double Latitude;
}

public class RideService
{
    private readonly IRideRepository _rideRepository;
    public RideService(IRideRepository rideRepository)
    {
        _rideRepository = rideRepository;
    }
    public async Task<bool> AcceptRideAsync(Ride ride, Guid driverId)
    {
        try
        {
            if (ride.DriverId != null)
            {
                throw new InvalidOperationException("Driver already assigned.");
            }
            var assigned = await _rideRepository.AssignDriverToRideAsync(ride.RideId, driverId, ride.DriverId);
            if (assigned)
            {
                ride.AssignDriver(driverId);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to accept ride {ride.RideId} for driver {driverId}, ex: {ex}");
            return false;
        }
    }
    public async Task<bool> UpdateDriverAsync(Ride ride, Guid driverId)
    {
        try
        {
            if (ride.DriverId == null)
            {
                throw new InvalidOperationException("Assign a driver before update.");
            }
            var assigned = await _rideRepository.AssignDriverToRideAsync(ride.RideId, driverId, ride.DriverId);
            if (assigned)
            {
                ride.AssignDriver(driverId);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update driver {driverId} for ride {ride.RideId} , ex: {ex}");
            return false;
        }
    }
}

public interface IRideRepository
{
    public Task<bool> AssignDriverToRideAsync(Guid rideId, Guid driverId, Guid? assignedDriverId);
}

public class RideRepository : IRideRepository
{
    public async Task<bool> AssignDriverToRideAsync(Guid rideId, Guid driverId, Guid? assignedDriverId = null)
    {
        // We will execute this sql or something similar using an ORM. 
        var sql = $"Update dbo.Ride Set DriverId = {driverId} where RideId = {rideId} and DriverId = {assignedDriverId}";
        // Assuming executing the sql will return the count of rows changed.
        int rowCount = 0;
        return rowCount > 0;
    }
}
