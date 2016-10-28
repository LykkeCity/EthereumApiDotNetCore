pragma solidity ^0.4.1;
import "./coin.sol";

contract TransferContract {
    address _owner;  

    modifier onlyowner { if (msg.sender == _owner) _; }

    function TransferContract() {        
        _owner = msg.sender;
    }

    function() payable {

    }

    function cashin(uint id, address coin, address receiver, uint amount, uint gas, bytes params) onlyowner {
         var coin_contract = Coin(coin);
         coin_contract.cashin.value(amount).gas(gas)(id, receiver, amount, params);
    }
}