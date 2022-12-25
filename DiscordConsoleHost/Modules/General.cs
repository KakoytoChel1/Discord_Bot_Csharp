using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordConsoleHost.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordConsoleHost.Modules
{
    public class General : ModuleBase<SocketCommandContext>
    {
        IConfiguration configuration;

        public General()
        {
            //initialize configuration
            configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", false, true)
                        .Build();
        }

        //test command 
        [Command("ping")]
        [Alias("p", "pi")]
        public async Task PingAsync()
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Channel.SendMessageAsync("Pong!");
        }

        //command to show order menu with 'Добавить' button
        [Command("getStartOrderMenu")]
        [Alias("getSOM")]
        public async Task Embed()
        {
            await Context.Channel.TriggerTypingAsync();
            await SendOrderEmbedAsync("Заказать постройку здания","Механизм что предоставляет возможность мэру облегчить свою жизнь," +
                " и заказать постройку у билдеров в министерстве. Чтобы создать новый раздел, и сформировать заказ нажмите на кнопку ниже.", Context.Message, Context.Channel);
        }

        private async Task SendOrderEmbedAsync(string title, string desc, SocketUserMessage message, ISocketMessageChannel channel)
        {
            var builder = new EmbedBuilder()
            {
                Color = Color.Green,
                Description = desc,
                Title = title
            };

            var btnBuilder = new ComponentBuilder()
                .WithButton(label: "🧱 Заказ", customId: configuration["OpenOrderMenu"], style:ButtonStyle.Secondary);

            await channel.SendMessageAsync("", false, builder.Build(), components:btnBuilder.Build());
        }
    }
}
