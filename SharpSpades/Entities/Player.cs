using SharpSpades.Net;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Player : Entity
    {
        public string Name => Client.Name;
        public Client Client { get; set; }

        internal Player(Client client)
        {
            Client = client;
        }

        internal override Task UpdateAsync(float delta)
        {
            return base.UpdateAsync(delta);
        }
    }
}
