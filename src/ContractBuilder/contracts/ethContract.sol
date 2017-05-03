pragma solidity ^0.4.9;
import "./coin.sol";

contract EthCoin is Coin(0) {

    function EthCoin(address exchangeContractAddress) Coin(exchangeContractAddress) { }

    function cashin(address receiver, uint amount) onlyowner payable {
        var userAddress = transferContractUser[receiver];

        if (userAddress == address(0)) {
            throw;
        } 
        coinBalanceMultisig[userAddress] += msg.value;

        CoinCashIn(receiver, msg.value);
    }

    function cashout(address client, address to, uint amount, bytes32 hash, bytes client_sig, bytes params) onlyFromExchangeContract {

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