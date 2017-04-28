pragma solidity ^0.4.1;
import "./coin.sol";

contract TransferBaseContract {
    address _owner;  
    address _userAddress
    address _coinAdapterAddress

    modifier onlyowner { if (msg.sender == _owner) _; }

    function TransferBaseContract(address userAddress, address coinAdapterAddress) {        
        _owner = msg.sender;
        _userAddress = userAddress;
        _coinAdapterAddress = coinAdapterAddress;
    }

    function() payable {
    }

    function kill() onlyowner{
        this.suicide(_owner)
    }

    function cashin(uint id, address coin, address receiver, uint amount, uint gas, bytes params) onlyowner {
        throw;
    }
}
