namespace SixDegrees.Model.JSON
{
    public partial class Place
    {
        public override bool Equals(object obj)
        {
            var other = obj as Place;
            if (other == null)
                return false;
            return Id.Equals(other.Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
