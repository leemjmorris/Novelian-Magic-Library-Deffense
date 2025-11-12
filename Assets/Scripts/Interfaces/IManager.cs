namespace NovelianMagicLibraryDefense.Core
{
    /// <summary>
    /// LMJ : Base interface for all manager classes
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// LMJ : Initialize manager resources and set up necessary dependencies
        /// </summary>
        void Initialize();

        /// <summary>
        /// LMJ : Reset manager to initial state without full destruction
        /// </summary>
        void Reset();

        /// <summary>
        /// LMJ : Clean up resources and release references
        /// </summary>
        void Dispose();
    }
}