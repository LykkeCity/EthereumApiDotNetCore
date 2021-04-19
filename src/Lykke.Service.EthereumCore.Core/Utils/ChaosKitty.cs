using System;

namespace Lykke.Service.EthereumCore.Core.Utils
{
    public static class ChaosKitty
    {
        private static readonly Random Randmom = new Random();
        private static double _stateOfChaos;

        public static double StateOfChaos
        {
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException();

                _stateOfChaos = value;
            }
        }

        public static void Meow()
        {
            if (_stateOfChaos < double.Epsilon)
                return;

            if (Randmom.NextDouble() < _stateOfChaos)
                throw new Exception("Meow");
        }

        public static bool MeowButLogically()
        {
            if (_stateOfChaos < double.Epsilon)
                return false;

            if (Randmom.NextDouble() < _stateOfChaos)
                return true;

            return false;
        }
    }
}











                                                                                                   
     //                                           /dd/                                                
     //                                         /dMMMMd/                                              
     //                                       /dMMMMMMMMd/                                            
     //                                     :dMMMMMMMMMMMMd:                                          
     //                                   `ohdddmMMMMMMddddho`                                        
     //                                    `````:MMMMMM:`````                                         
     //             .oooooooooooo/              -MMMMMM-              /oooooooooooo.
     //             /MMMMMMMMMMm+.              -MMMMMM-              .+mMMMMMMMMMM/                  
     //             /MMMMMMMMMo.                -MMMMMM-                .oMMMMMMMMM/                  
     //             /MMMMMMMMMh:`               -MMMMMM-               `:hMMMMMMMMM/                  
     //             /MMMMMMMMMMMh:              -MMMMMM-              :hMMMMMMMMMMM/                  
     //             /MMms/dMMMMMMMh:`           -MMMMMM-           `:hMMMMMMMd/smMM/                  
     //             /Ns.  `/dMMMMMMNh:          -MMMMMM-          :hNMMMMMMd/`  .sN/                  
     //             ..      `/dMMMMMMMy-        -MMMMMM-        -yMMMMMMMd/`      ..                  
     //                       `/dMMMMMMNy-      -MMMMMM-      -yNMMMMMMd/`                            
     //                         `/mMMMMMMNy-    -MMMMMM-    -yNMMMMMMm/`                              
     //                           `+mMMMMMMNy-  -MMMMMM-  -yNMMMMMMm+`                                
     //         `.                  `+mMMMMMMNs.:MMMMMM:.sNMMMMMMm+`                  .`              
     //       `om/                    `+mMMMMMMNhMMMMMMhNMMMMMMm+`                    /mo`            
     //     `+NMM/                      `+NMMMMMMMMMMMMMMMMMMN+`                      /MMN+`          
     //   `+mMMMM+........................-yMMMMMMMMMMMMMMMMy-........................+MMMMm+`        
     // `+mMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMm+`      
     //`hMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMh`     
     // `+mMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMm+`      
     //   `+mMMMM+........................-yMMMMMMMMMMMMMMMMy-........................+MMMMm+`        
     //     `+NMM/                      `+NMMMMMMMMMMMMMMMMMMN+`                      /MMN+`          
     //       `om/                    `+mMMMMMMNhMMMMMMhNMMMMMMm+`                    /mo`            
     //         `.                  `+mMMMMMMNs.:MMMMMM:.sNMMMMMMm+`                  .`              
     //                           `+mMMMMMMNy-  -MMMMMM-  -yNMMMMMMm+`                                
     //                         `/mMMMMMMNy-    -MMMMMM-    -yNMMMMMMm/`                              
     //                       `/dMMMMMMNy-      -MMMMMM-      -yNMMMMMMd/`                            
     //             ..      `/dMMMMMMMy-        -MMMMMM-        -yMMMMMMMd/`      ..                  
     //             /Ns.  `/dMMMMMMNh:          -MMMMMM-          :hNMMMMMMd/`  .sN/                  
     //             /MMms/dMMMMMMMh:`           -MMMMMM-           `:hMMMMMMMd/smMM/                  
     //             /MMMMMMMMMMMh:              -MMMMMM-              :hMMMMMMMMMMM/                  
     //             /MMMMMMMMMh:`               -MMMMMM-               `:hMMMMMMMMM/                  
     //             /MMMMMMMMMo.                -MMMMMM-                .oMMMMMMMMM/                  
     //             /MMMMMMMMMMm+.              -MMMMMM-              .+mMMMMMMMMMM/                  
     //             .oooooooooooo/              -MMMMMM-              /oooooooooooo.
     //                                    `````:MMMMMM:`````                                         
     //                                   `ohdddmMMMMMMmdddho`                                        
     //                                     :dMMMMMMMMMMMMd:                                          
     //                                       /dMMMMMMMMd/                                            
     //                                         /dMMMMd/                                              
     //                                           /dd/                                                
                                                                                                    
                                                                                                    
                                                                                                  
