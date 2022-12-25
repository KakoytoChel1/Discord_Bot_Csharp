using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordConsoleHost.Model;
using Microsoft.Extensions.Configuration;

namespace DiscordConsoleHost.Modules
{
    public class InterModule : InteractionModuleBase<SocketInteractionContext>
    {
        Random rnd;
        public InteractionService Commands { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<Customer> Customers;
        IConfiguration configuration;

        public InterModule()
        {
            //initialize configuration
            configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", false, true)
                        .Build();
            rnd = new Random();
            DataBaseLogic.StartSettings();
            Customers = new System.Collections.ObjectModel.ObservableCollection<Customer>(DataBaseLogic.GetCustomers());
        }

        [SlashCommand("get_order_menu", "Send special order menu for mayors")]
        public async Task SendOrderMenu()
        {
            var guildUser = Context.User as SocketGuildUser;

            if (guildUser.GuildPermissions.Administrator)
            {
                var embed = new EmbedBuilder()
                {
                    Color = Color.Green,
                    Description = "Механизм что предоставляет возможность мэру облегчить свою жизнь," +
                  " и заказать постройку у билдеров в министерстве. Чтобы создать новый раздел, и сформировать заказ нажмите на кнопку ниже.",
                    Title = "Заказать постройку здания"
                };

                var btnBuilder = new ComponentBuilder()
                    .WithButton(label: "🧱 Заказ", customId: "OpenOrderMenu_Id", style: ButtonStyle.Secondary);

                //await channel.SendMessageAsync("", false, builder.Build(), components: btnBuilder.Build());

                await RespondAsync(embed: embed.Build(), components: btnBuilder.Build());
            }

        }

        [ComponentInteraction("OpenOrderMenu_Id")]
        public async Task OpenOrderMenu()
        {
            ulong componentChannelId = Context.Channel.Id;

            //get that channel
            var textChannel = Context.Client.GetChannel(componentChannelId) as SocketTextChannel;
            //get guild of the all channels
            var currentGuild = Context.Client.GetGuild(textChannel.Category.GuildId);
            //get current category id by the name
            var categoryId = currentGuild.CategoryChannels.First(c => c.Name == "For Orders").Id;


            //create new channel for customer's order
            var newOrderChannel = await currentGuild.CreateTextChannelAsync($"заказ-id-{rnd.Next(1, 1000)}", tcp => tcp.CategoryId = categoryId);

            //set customer permissions
            await newOrderChannel.AddPermissionOverwriteAsync(Context.User, OverwritePermissions.DenyAll(newOrderChannel)
                .Modify(sendMessages: PermValue.Allow, attachFiles: PermValue.Allow, viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));
            //set perms for all mayors except current customer (who had created new channel with order)
            await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.GetRole(1051902792429752320), OverwritePermissions.DenyAll(newOrderChannel));
            //set builder permissions
            await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.GetRole(1051896050341912596), OverwritePermissions.DenyAll(newOrderChannel)
                .Modify(sendMessages: PermValue.Deny, attachFiles: PermValue.Allow, viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));

            //set permission for everyone
            await newOrderChannel.AddPermissionOverwriteAsync(currentGuild.EveryoneRole, OverwritePermissions.DenyAll(newOrderChannel));

            //add a customer to the database who just placed an order
            Customer custm = new Customer { ChannelId = newOrderChannel.Id, CustomerId = Context.User.Id, CustomerName = Context.User.Username };
            Customers.Add(custm);
            DataBaseLogic.AddNewCustomer(custm);

            //send list with requirements to just created channel (that new channel)
            await SendOrderRequirementsEmbedAsync("Форма заказа",
                "1) Ник в майнкрафте\n" +
                "2) Город\n" +
            "3) Несколько скринов (не больше 3)\n", Context, currentGuild, newOrderChannel);

            await RespondAsync();

        }

        [ComponentInteraction("CloseOrderMenu_Id")]
        public async Task CloseOrderMenu()
        {
            //get id of the channel where click came from
            ulong componentChannelId = Context.Channel.Id;

            //get that channel
            var textChannel = Context.Client.GetChannel(componentChannelId) as SocketTextChannel;
            //get guild of the all channels
            var currentGuild = Context.Client.GetGuild(textChannel.Category.GuildId);
            //get current category id by the name
            var categoryId = currentGuild.CategoryChannels.First(c => c.Name == "For Orders").Id;

            //find specific customer by channel id
            Customer? item = Customers.Where(i => i.ChannelId == componentChannelId).FirstOrDefault();

            //delete customer from database and collection like a text channel if button was clicked by who created this channel
            if (item != null && Context.User.Id == item.CustomerId)
            {
                Customers.Remove(item);
                DataBaseLogic.RemoveCustomer(item);
                await textChannel.DeleteAsync();
            }

            await RespondAsync();
        }

        [ComponentInteraction("TakeOrder_Id")]
        public async Task TakeOrder()
        {
            //get id of the channel where click came from
            ulong componentChannelId = Context.Channel.Id;

            //get that channel
            var textChannel = Context.Client.GetChannel(componentChannelId) as SocketTextChannel;
            //get guild of the all channels
            var currentGuild = Context.Client.GetGuild(textChannel.Category.GuildId);
            //get current category id by the name
            var categoryId = currentGuild.CategoryChannels.First(c => c.Name == "For Orders").Id;

            //find specific customer by channel id
            Customer? item = Customers.Where(i => i.ChannelId == componentChannelId).FirstOrDefault();

            var clickedUser = (Context.User as SocketGuildUser);
            //builder role
            var builderRole = currentGuild.GetRole(1051896050341912596);

            //check if user who clicked contains builder role
            if (clickedUser.Roles.Contains(builderRole))
            {
                await textChannel.SendMessageAsync($"{Context.Client.GetUser(item.CustomerId).Mention} ваш заказ был взят {Context.User.Mention}, отпишите ему в лс!");
            }

            await RespondAsync();
        }

        //sending panel with requirements to recently created channel
        private async Task SendOrderRequirementsEmbedAsync(string title, string desc, SocketInteractionContext context, SocketGuild category, RestTextChannel newChannel)
        {
            //form the panel itself
            var embed = new EmbedBuilder()
            {
                Color = Color.Green,
                Description = desc,
                Title = title
            };
            embed.AddField("Примечание", "Данный механизм придназначен для непосредственного оповещения строителей и упращения комуникации с мэрами " +
                "городов. По этой причине обмен схематиками доступен исключительно в лс.");

            //form buttons under panel
            var btnBuilder = new ComponentBuilder()
                .WithButton(label: "📌Принять", customId: configuration["TakeOrder"], style: ButtonStyle.Secondary)
                .WithButton(label: "🔒Закрыть", customId: configuration["CloseOrderMenu"], style: ButtonStyle.Secondary);

            //send one message with panel and buttons
            await newChannel.SendMessageAsync($"Приветствую {context.User.Mention}, обладатели роли {category.GetRole(1051896050341912596).Mention}" +
                $" были оповещены", false, embed.Build(), components: btnBuilder.Build());
        }

    }
}
