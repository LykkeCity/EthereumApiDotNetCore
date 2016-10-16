pragma solidity ^0.4.1;
import "./coin.sol";

contract EthCoin is Coin(0) {

    function EthCoin(address exchangeContractAddress) Coin(exchangeContractAddress) { }

    function cashin(uint id, address receiver, uint amount) onlyowner payable {

        if (transactions[id])
            throw;
        
        coinBalanceMultisig[receiver] += msg.value;

        CoinCashIn(receiver, msg.value);

        transactions[id] = true;
    }

    function cashout(address client, address to, uint amount, bytes32 hash, bytes client_sig) onlyFromExchangeContract {

        if (!_checkClientSign(client, hash, client_sig)) {
            throw;                    
        }

        if (coinBalanceMultisig[client] < amount) {
            throw;
        }

        coinBalanceMultisig[client] -= amount;

        if (!to.send(amount)) throw;

        CoinCashOut(msg.sender, client, amount, to);
    }
}