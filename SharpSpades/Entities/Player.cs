using SharpSpades.Net;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Player : Entity
    {
        public string Name => this.Client.Name;

        public Client Client { get; set; }

        internal Player(Client client) => this.Client = client;

        internal override Task UpdateAsync(float delta) => base.UpdateAsync(delta);
    }
}
