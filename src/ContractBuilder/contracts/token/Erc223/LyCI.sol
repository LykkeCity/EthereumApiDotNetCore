pragma solidity ^0.4.11;
import "./erc223Token.sol";
import "./lykkeTokenErc223Base.sol";
import "./SafeMath.sol";
import "./emissiveErc223Token.sol";

contract LyCI is EmissiveErc223Token {
    using SafeMath for uint;
    string public termsAndConditionsUrl;
    address public owner;

    function LyCI(
        address issuer,
        string tokenName,
        uint8 divisibility,
        string tokenSymbol, 
        string version) EmissiveErc223Token(issuer, tokenName, divisibility, tokenSymbol, version) public{
        owner = msg.sender;
    }

    function getTermsAndConditions () public view returns (string tc) {
        return termsAndConditionsUrl;
    }

    function setTermsAndConditions (string _newTc) public {
        if (msg.sender != owner){
            revert("Only owner is allowed to change T & C");
        }
        termsAndConditionsUrl = _newTc;
    }
}
