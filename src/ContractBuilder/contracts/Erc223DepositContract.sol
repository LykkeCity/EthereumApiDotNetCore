pragma solidity ^0.4.11;

import "./token/erc20Contract.sol";

contract Erc223DepositContract {
    
    address public _owner = msg.sender;

    modifier onlyOwner { 
        if (msg.sender == _owner) {
            _;
        }
    }

    function() public payable {
        throw;
    }

    function transferTokens(address _tokenAddress, address _to, uint256 _amount) onlyOwner public returns (bool success) {
        
        ERC20Interface erc20Contract = ERC20Interface(_tokenAddress);
        uint balance = erc20Contract.balanceOf(this); 

        if (_amount <= 0)
        {
            return;
        }

        if (_amount > balance || _to == address(this)) {
            return false;
        }

        return erc20Contract.transfer(_to, _amount);
    }

    //Add compatibility for erc223 contract reciever
    function tokenFallback(address _from, uint _value, bytes _data) public pure returns(bool ok) {
        return true;
    }
}