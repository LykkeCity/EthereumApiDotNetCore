pragma solidity ^0.4.1;
import "./coin.sol";

contract EthCoin is Coin(0) {

    function EthCoin(address exchangeContractAddress) Coin(exchangeContractAddress) { }

    function cashin(address receiver, uint amount) onlyowner {

        coinBalanceMultisig[receiver] += msg.value;

        CoinCashIn(receiver, msg.value);
    }

    function cashout(address client, address to, uint amount, bytes32 hash, bytes client_sig) onlyFromExchangeContract {

        if (!_checkClientSign(client, hash, client_sig)) {
            throw;                    
        }

        if (coinBalanceMultisig[client] < amount) {
            throw;
        }

        coinBalanceMultisig[client] -= amount;

        to.send(amount);

        CoinCashOut(msg.sender, client, amount, to);
    }
}