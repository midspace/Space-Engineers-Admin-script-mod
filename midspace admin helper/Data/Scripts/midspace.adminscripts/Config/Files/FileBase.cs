using Sandbox.ModAPI;

namespace midspace.adminscripts.Config.Files
{
    public abstract class FileBase
    {
        public string Name { get; protected set; }

        protected FileBase(string fileName, string format)
        {
            Name = string.Format(format, fileName);
            Init();
        }

        protected FileBase() { }

        public abstract void Save(string customSaveName = null);
        public abstract void Load();
        public abstract void Create();

        protected void Init()
        {
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Name, typeof (FileBase)))
                Load();
            else
                Create();
        }
    }
}