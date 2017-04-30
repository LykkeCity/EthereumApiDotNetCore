pragma solidity ^0.4.1;
import "./coin.sol";
import "./transferBaseContract.sol";

contract TokenTransferContract is TransferBaseContract{

    modifier onlyowner { if (msg.sender == _owner) _; }

    function TokenTransferContract(address userAddress, address coinAdapterAddress) TransferBaseContract(userAddress, coinAdapterAddress) {        
    }

    function cashin(uint id, address coin, address receiver, uint amount, uint gas, bytes params) onlyowner {
        var coin_contract = Coin(coin);
        coin_contract.cashin.gas(gas)(id, receiver, amount, params);
    }
}
