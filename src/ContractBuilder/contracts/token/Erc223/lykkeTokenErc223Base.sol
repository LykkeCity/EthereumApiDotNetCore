pragma solidity ^0.4.11;

import "./erc223Token.sol";

contract LykkeTokenErc223Base is ERC223Token {

    address internal _issuer;
    string public standard;
    string public name;
    string public symbol;
    uint8 public decimals;

    function LykkeTokenErc223Base(
        address issuer,
        string tokenName,
        uint8 divisibility,
        string tokenSymbol, 
        string version,
        uint256 totalSupply) ERC223Token(totalSupply) public{
        symbol = tokenSymbol;
        standard = version;
        name = tokenName;
        decimals = divisibility;
        _issuer = issuer;
    }
}
