using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordConsoleHost.Services
{
    public class InteractionHandler
    {
        private readonly IServiceProvider provider;
        private readonly DiscordSocketClient client;
        private readonly InteractionService commands;
        private readonly IConfiguration configuration;

        public InteractionHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider,
            InteractionService commands, IConfiguration configuration)
        {
            this.provider = provider;
            this.client = client;
            this.commands = commands;
            this.configuration = configuration;
        }
        public async Task InitializeAsync()
        {
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);

            client.InteractionCreated += Client_InteractionCreated;
            //client.SlashCommandExecuted += Client_SlashCommandExecuted;

            commands.SlashCommandExecuted += Commands_SlashCommandExecuted;
            commands.ComponentCommandExecuted += Commands_ComponentCommandExecuted;
            commands.ContextCommandExecuted += Commands_ContextCommandExecuted;
        }

        private async Task<Task> Commands_ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            return Task.CompletedTask;
        }

        private async Task<Task> Commands_ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            return Task.CompletedTask;
        }

        private async Task<Task> Commands_SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            

            return Task.CompletedTask;
        }



        private async Task Client_InteractionCreated(SocketInteraction interaction)
        {
            var scope = provider.CreateScope();

            var ctx = new SocketInteractionContext(client, interaction);

            await commands.ExecuteCommandAsync(ctx, scope.ServiceProvider);
        }
    }
}
