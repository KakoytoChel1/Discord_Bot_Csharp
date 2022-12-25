using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordConsoleHost.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiscordConsoleHost.Services
{
    public class CommandHandler : DiscordClientService
    {
        Random rnd;
        private readonly IServiceProvider provider;
        private readonly DiscordSocketClient client;
        private readonly CommandService service;
        private readonly IConfiguration configuration;

        public ObservableCollection<Customer> Customers { get; set; }

        //set constructor to get some fields and set basic settings
        public CommandHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider,
            CommandService service, IConfiguration configuration) : base(client, logger)
        {
            this.provider = provider;
            this.client = client;
            this.service = service;
            this.configuration = configuration;

            rnd = new Random();
            DataBaseLogic.StartSettings();
            Customers = new ObservableCollection<Customer>(DataBaseLogic.GetCustomers());
        }

        //set all events what we need, and activate modules
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.client.MessageReceived += OnMessageReceived;
            this.client.ButtonExecuted += Client_ButtonExecuted;
            await this.service.AddModulesAsync(Assembly.GetEntryAssembly(), this.provider);
        }

        //method for ButtonExpected event
        private async Task Client_ButtonExecuted(SocketMessageComponent component)
        {
            //get id of the channel where click came from
            ulong componentChannelId = component.Channel.Id;

            //get that channel
            var textChannel = client.GetChannel(componentChannelId) as SocketTextChannel;
            //get guild of the all channels
            var currentGuild = client.GetGuild(textChannel.Category.GuildId);
            //get current category id by the name
            var categoryId = currentGuild.CategoryChannels.First(c => c.Name == "For Orders").Id;

            //check what was command executed
            if (component.Data.CustomId == configuration["OpenOrderMenu"])
            {
                //create new channel for customer's order
                var newOrderChannel = await currentGuild.CreateTextChannelAsync($"заказ-id-{rnd.Next(1, 1000)}", tcp => tcp.CategoryId = categoryId);

                //set customer permissions
                await newOrderChannel.AddPermissionOverwriteAsync(component.User, OverwritePermissions.DenyAll(newOrderChannel)
                    .Modify(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));
                //set perms for all mayors except current customer (who had created new channel with order)
                await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.GetRole(1051902792429752320), OverwritePermissions.DenyAll(newOrderChannel));
                //set builder permissions
                await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.GetRole(1051896050341912596), OverwritePermissions.DenyAll(newOrderChannel)
                    .Modify(sendMessages: PermValue.Deny, attachFiles: PermValue.Allow, viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));

                //set permission for everyone
                await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.EveryoneRole, OverwritePermissions.DenyAll(newOrderChannel));

                //add a customer to the database who just placed an order
                Customer custm = new Customer { ChannelId = newOrderChannel.Id, CustomerId = component.User.Id, CustomerName = component.User.Username };
                Customers.Add(custm);
                DataBaseLogic.AddNewCustomer(custm);

                //send list with requirements to just created channel (that new channel)
                await SendOrderRequirementsEmbedAsync("Форма заказа",
                    "1) Ник в майнкрафте\n" +
                    "2) Город\n" +
                    "3) Несколько скринов (не больше 3)\n" +
                    "4) Файл схематики", component, currentGuild, newOrderChannel);

                //await component.RespondAsync("Succesfully completed!");
            }
            if (component.Data.CustomId == configuration["CloseOrderMenu"])
            {
                //find specific customer by channel id
                Customer? item = Customers.Where(i => i.ChannelId == componentChannelId).FirstOrDefault();
                
                //delete customer from database and collection like a text channel if button was clicked by who created this channel
                if(item != null && component.User.Id == item.CustomerId)
                {
                    Customers.Remove(item);
                    DataBaseLogic.RemoveCustomer(item);
                    await textChannel.DeleteAsync();
                }
            }
            else if (component.Data.CustomId == configuration["TakeOrder"])
            {
                
                //find specific customer by channel id
                Customer? item = Customers.Where(i => i.ChannelId == componentChannelId).FirstOrDefault();

                var clickedUser = (component.User as SocketGuildUser);
                //builder role
                var builderRole = currentGuild.GetRole(1051896050341912596);

                //check if user who clicked contains builder role
                if (clickedUser.Roles.Contains(builderRole))
                {
                    await textChannel.SendMessageAsync($"{client.GetUser(item.CustomerId).Mention} ваш заказ был взят {component.User.Mention}, отпишите ему в лс!");
                }
            }
        }

        //method for MessageReceived event
        private async Task OnMessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message)) return;

            if (message.Source != Discord.MessageSource.User) return;

            var argPos = 0;

            if (!message.HasStringPrefix(configuration["Prefix"], ref argPos) && !message.HasMentionPrefix(this.client.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(this.client, message);
            await this.service.ExecuteAsync(context, argPos, this.provider);
        }

        //sending panel with requirements to recently created channel
        private async Task SendOrderRequirementsEmbedAsync(string title, string desc, SocketMessageComponent component , SocketGuild category, RestTextChannel newChannel)
        {
            //form the panel itself
            var builder = new EmbedBuilder()
            {
                Color = Color.Green,
                Description = desc,
                Title = title
            };

            //form buttons under panel
            var btnBuilder = new ComponentBuilder()
                .WithButton(label: "Принять", customId: configuration["TakeOrder"], style: ButtonStyle.Success)
                .WithButton(label: "Отмена", customId: configuration["CloseOrderMenu"], style: ButtonStyle.Danger);

            //send one message with panel and buttons
            await newChannel.SendMessageAsync($"{component.User.Mention}, {category.GetRole(1051896050341912596).Mention}", false, builder.Build(), components: btnBuilder.Build());
        }

        //sending private message (don't using)
        private async Task SendPrivateMessage(IUser user, string message)
        {
            var channell = await user.CreateDMChannelAsync();
            await channell.SendMessageAsync(message);
        }
    }
}
