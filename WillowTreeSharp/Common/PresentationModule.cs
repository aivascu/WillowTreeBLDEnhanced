using Autofac;
using WillowTree.Controls;
using WillowTree.Plugins;
using WillowTree.Presenters;

namespace WillowTree.Common
{
    public class PresentationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MessageBoxWrapper>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PluginComponentManager>().AsSelf().SingleInstance();
            builder.RegisterType<AppThemes>().AsSelf().SingleInstance();
            builder.RegisterType<WillowTreeMain>().AsSelf().SingleInstance();

            builder
                .RegisterAssemblyTypes(this.ThisAssembly)
                .Where(x => x.IsAssignableTo<IPlugin>())
                .AsSelf().SingleInstance();

            builder
                .RegisterType<AboutViewPresenter>()
                .AsSelf().SingleInstance();
        }
    }
}
