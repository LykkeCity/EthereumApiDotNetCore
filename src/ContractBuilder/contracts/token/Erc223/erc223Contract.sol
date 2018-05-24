pragma solidity ^0.4.11;

contract ERC223Interface {
      
    function balanceOf(address _who) view public returns (uint);
    function transfer(address _to, uint _value) public returns (bool success);
    function transfer(address _to, uint _value, bytes _data) public returns (bool success);
    function transferFrom(address _from, address _to, uint256 _value) public returns (bool success);
    function approve(address _spender, uint256 _value) public returns (bool success);
    function allowance(address _owner, address _spender) public view returns (uint256 remaining);
    function totalSupply() public view returns (uint256 supply);

    event Transfer(address indexed _from, address indexed _to, uint _value);
    event Transfer(address indexed _from, address indexed _to, uint _value, bytes _data);
    event Approval(address indexed _from, address indexed _spender, uint256 _value);
    
}