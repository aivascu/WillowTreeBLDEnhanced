using System.IO.Abstractions;
using Autofac;

namespace WillowTree.Common
{
    public class FileSystemModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FileSystem>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<FileWrapper>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DirectoryWrapper>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
