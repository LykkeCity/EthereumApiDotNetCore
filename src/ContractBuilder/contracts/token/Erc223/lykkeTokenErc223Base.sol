pragma solidity ^0.4.11;

import "./erc223Token.sol";

contract LykkeTokenErc223Base is ERC223Token {

    address internal _issuer;
    string public standard;

    function LykkeTokenErc223Base(
        address issuer,
        string tokenName,
        uint8 divisibility,
        string tokenSymbol, 
        string version,
        uint256 totalSupply) ERC223Token(tokenName, tokenSymbol, divisibility, totalSupply) public{
        standard = version;
        _issuer = issuer;
    }
}
