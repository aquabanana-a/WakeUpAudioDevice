namespace WakeUpAudioDevice.tools
{
    internal static class KotlinStyleExtensions
    {
        public static T Apply<T>(this T obj, Action<T> block)
        {
            block(obj);
            return obj;
        }
    }
}