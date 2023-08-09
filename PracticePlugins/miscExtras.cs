using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticePlugins
{
    public class miscExtras
    {
        public static bool IsGun(ItemType type)
        {
            bool result = false;
            switch (type)
            {
                case ItemType.GunCOM15:
                    result = true;
                    break;
                case ItemType.GunCOM18:
                    result = true;
                    break;
                case ItemType.GunCom45:
                    result = true;
                    break;
                case ItemType.GunFSP9:
                    result = true;
                    break;
                case ItemType.GunCrossvec:
                    result = true;
                    break;
                case ItemType.GunE11SR:
                    result = true;
                    break;
                case ItemType.GunAK:
                    result = true;
                    break;
                case ItemType.GunRevolver:
                    result = true;
                    break;
                case ItemType.GunShotgun:
                    result = true;
                    break;
                case ItemType.GunLogicer:
                    result = true;
                    break;
                case ItemType.ParticleDisruptor:
                    result = true;
                    break;
            }
            return result;
        }
    }
}
