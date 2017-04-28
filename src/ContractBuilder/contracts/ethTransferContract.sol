pragma solidity ^0.4.1;
import "./coin.sol";
import "./transferBaseContract.sol";

contract EthTransferContract {

    modifier onlyowner { if (msg.sender == _owner) _; }

    function EthTransferContract(address userAddress, address coinAdapterAddress) TransferBaseContract(address userAddress, address coinAdapterAddress) {        
    }

    function() payable{
    }

    function cashin(uint id, address coin, address receiver, uint amount, uint gas, bytes params) onlyowner {
        if (this.balance <= 0)
        throw;

        var coin_contract = Coin(coin);
        coin_contract.cashin.value(amount).gas(gas)(id, receiver, amount, params);
    }
}
