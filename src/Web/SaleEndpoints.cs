public static class SaleEndpoints
{
	public static IResult List() => Results.Ok("Sales list endpoint");
	public static IResult Add() => Results.Ok("Add sale endpoint");
}
