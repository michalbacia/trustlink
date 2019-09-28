namespace Trustlink.IO
{
    public interface ICloneable<T>
    {
        T Clone();
        void FromReplica(T replica);
    }
}
