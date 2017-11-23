pragma solidity ^0.4.11;

import "./token/erc20Contract.sol";

contract Erc20DepositContract {
    
    address public _owner = msg.sender;

    modifier onlyOwner { 
        if (msg.sender == _owner) {
            _;
        }
    }

    function transferAllTokens(address _tokenAddress, address _to) onlyOwner public returns (bool success) {
        
        ERC20Interface erc20Contract = ERC20Interface(_tokenAddress);
        uint balance = erc20Contract.balanceOf(this); 

        if (balance <= 0 || _to == address(this)) {
            return false;
        }

        return erc20Contract.transferFrom(this, _to, balance);
    }
}