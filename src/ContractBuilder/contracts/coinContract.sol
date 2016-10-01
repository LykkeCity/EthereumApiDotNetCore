pragma solidity ^0.4.1;
import "./coin.sol";

contract ColorCoin is Coin(0){

    function ColorCoin(address exchangeContractAddress) Coin(exchangeContractAddress) { }

    function cashin(address receiver, uint amount) onlyowner payable {

        if (msg.value > 0) throw; 
        
        coinBalanceMultisig[receiver] += amount;

        CoinCashIn(receiver, amount);
    }

    // cashout coins (called only from exchange contract)
    function cashout(address client, address to, uint amount, bytes32 hash, bytes client_sig) onlyFromExchangeContract { 

        if (!_checkClientSign(client, hash, client_sig)) {
            throw;                    
        }

        if (coinBalanceMultisig[client] < amount) {
            throw;
        }

        coinBalanceMultisig[client] -= amount;

        CoinCashOut(msg.sender, client, amount, to);
    }
}