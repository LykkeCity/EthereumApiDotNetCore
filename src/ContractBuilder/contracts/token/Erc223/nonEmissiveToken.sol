pragma solidity ^0.4.11;

import "./erc223Token.sol";
import "./lykkeTokenErc223Base.sol";

contract NonEmissiveErc223Token is LykkeTokenErc223Base {

    uint256 internal _initialSupply;

    function NonEmissiveErc223Token(
        address issuer,
        string tokenName,
        uint8 divisibility,
        string tokenSymbol, 
        string version, 
        uint256 initialSupply) LykkeTokenErc223Base(issuer, tokenName, divisibility, tokenSymbol, version, _initialSupply) public {
        bytes memory empty;

        _initialSupply = initialSupply;
        balances [_issuer] = initialSupply;
        
        emit Transfer(address(0), _issuer, initialSupply, empty);
        emit Transfer (address(0), _issuer, initialSupply);
    }

    function totalSupply () view public returns (uint256 supply) {
        return _initialSupply;
    }

    function balanceOf (address _owner) view public returns (uint256 balance) {
        return ERC223Token.balanceOf (_owner);
    }
}
