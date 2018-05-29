pragma solidity ^0.4.11;
import "./erc223Token.sol";
import "./emissiveErc223Token.sol";
import "./lykkeTokenErc223Base.sol";
import "./SafeMath.sol";

contract EmissiveErc223TokenWithParameters is EmissiveErc223Token {
    using SafeMath for uint;
    
    function EmissiveErc223TokenWithParameters() EmissiveErc223Token(address(0x000), "TokenName", 18, "LKT", "1.0.0") public{
    }
}
