using SharpSpades.Api.Entities;
using SharpSpades.Api.Net;
using System.Threading.Tasks;

namespace SharpSpades.Entities
{
    public class Player : Entity, IPlayer
    {
        public string Name { get; internal set; }
        public bool IsAlive { get; private set; }
        public IClient Client { get; internal set; }

        internal Player(IClient client)
        {
            Client = client;
        }

        internal override Task UpdateAsync(float delta)
        {
            
            return base.UpdateAsync(delta);
        }
    }
}
