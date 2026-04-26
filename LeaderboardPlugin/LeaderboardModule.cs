using AssettoServer.Server.Plugin;
using Autofac;
using Microsoft.Extensions.Hosting;

namespace LeaderboardPlugin;

public class LeaderboardModule : AssettoServerModule<LeaderboardConfiguration>
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<Leaderboard>().AsSelf().As<IHostedService>().SingleInstance();
    }
}
