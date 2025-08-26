public static class MilkCollectionEndpoints
{
	public static IResult List() => Results.Ok("Milk collections list endpoint");
	public static IResult Add() => Results.Ok("Add milk collection endpoint");
	public static IResult Update() => Results.Ok("Update milk collection endpoint");
	public static IResult Delete() => Results.Ok("Delete milk collection endpoint");
}
