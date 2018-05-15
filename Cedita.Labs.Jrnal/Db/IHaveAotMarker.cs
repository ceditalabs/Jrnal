namespace Cedita.Labs.Jrnal.Db
{
    public interface IHaveAotMarker
    {
        int AotInsertionMarker { get; set; }
    }

    public interface IHaveChildAotMarker
    {
        int AotParentMarker { get; set; }
    }
}
