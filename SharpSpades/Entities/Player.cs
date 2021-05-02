using SharpSpades.Net;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Player : Entity
    {
        public string Name { get; internal init; } // TODO: Make setter public?
        public bool IsAlive { get; internal set; }
        public Client Client { get; internal set; }

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
