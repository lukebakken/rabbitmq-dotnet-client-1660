using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Genie.Common.Settings;
public class RabbitMQSettings(string exchange, string queue, string routingKey, string user, string pass, string vhost, string host)
{
    public string Exchange => exchange;
    public string Queue => queue;
    public string RoutingKey => routingKey;
    public string Host => host;
    public string User => user;
    public string Pass => pass;
    public string Vhost => vhost;
}