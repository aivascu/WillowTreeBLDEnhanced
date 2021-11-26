using Autofac;
using WillowTree.Inventory;
using WillowTree.Services.Configuration;
using WillowTree.Services.DataAccess;

namespace WillowTree.Common
{
    public class ServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GameDataWrapper>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<InventoryDataWrapper>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<XmlCacheWrapper>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GlobalSettingsWrapper>().AsImplementedInterfaces().SingleInstance();
        }
    }
}
