using Microsoft.AspNetCore.Mvc;
using SecureFlight.Api.Utils;
using SecureFlight.Core.DataTransferObjects;
using SecureFlight.Core.Entities;
using SecureFlight.Core.Interfaces;
using SecureFlight.Core.Mapping;

namespace SecureFlight.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FlightsController(
    IService<Flight> flightService,
    IService<Passenger> passengerService
    )
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FlightDataTransferObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get()
    {
        var flights = await flightService.GetAllAsync();
        return flights.Succeeded
            ? Ok(flights.Result.Select(x => x.ToFlightDto()))
            : Problem(flights.ErrorResult.Message, statusCode: flights.ErrorResult.Code.ToHttpStatusCode(), title: "Error Retrieving Flights");
    }

    [HttpPut("AddPassenger")]
    [ProducesResponseType(typeof(FlightDataTransferObject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put(long flightId, string passengerId)
    {
        var flight = await flightService.FindAsync(flightId);
        var passenger = await passengerService.FindAsync(passengerId);

        if (!flight.Succeeded)
        {
            return Problem("Flight not found");
        } 

        if(passenger.Succeeded && passenger.Result.PassengerFlights.Count > 0)
        {
            Problem("Passenger is in another fligh");
        }

        if(passenger.Result.Flights.Where(x => x.Id == flightId).Count() > 0)
        {
            Problem("Passenger already in the flight");
        }

        passenger.Result.Flights.Add(flight.Result);
        flight.Result.Passengers.Add(passenger.Result);

        var passengerUpdate = passengerService.Update(passenger);
        var flightUpdate = flightService.Update(flight);

        return passengerUpdate.Succeeded ? Ok() : Problem("Error while adding the passenger to a flight");
    }
}