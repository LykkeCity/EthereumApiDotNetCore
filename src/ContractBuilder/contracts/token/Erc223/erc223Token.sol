pragma solidity ^0.4.11;

import "./contractReceiver.sol";
import "./erc223Contract.sol";
import "./SafeMath.sol";

 /**
 * ERC223 token by Dexaran
 *
 * https://github.com/Dexaran/ERC223-token-standard
 */ 


/**
 * @title Reference implementation of the ERC223 standard token.
 */
contract ERC223Token is ERC223Interface {
    using SafeMath for uint;

    mapping(address => uint) balances; // List of user balances.
    mapping (address => mapping (address => uint256)) private allowances;
    
    string public name;
    string public symbol;
    uint8 public decimals;
    uint256 public supply;
    
    function ERC223Token(string _name, string _symbol, uint8 _decimals, uint256 _totalSupply) public
    {
        name = _name;
        symbol = _symbol;
        decimals = _decimals;
        supply = _totalSupply;
    }       

    /**
     * @dev Transfer the specified amount of tokens to the specified address.
     *      Invokes the `tokenFallback` function if the recipient is a contract.
     *      The token transfer fails if the recipient is a contract
     *      but does not implement the `tokenFallback` function
     *      or the fallback function to receive funds.
     *
     * @param _to    Receiver address.
     * @param _value Amount of tokens that will be transferred.
     * @param _data  Transaction metadata.
     */
    function transfer(address _to, uint _value, bytes _data) public returns (bool success){
        // Standard function transfer similar to ERC20 transfer with no _data .
        // Added due to backwards compatibility reasons .
        uint codeLength;

        assembly {
            // Retrieve the size of the code on target address, this needs assembly .
            codeLength := extcodesize(_to)
        }

        balances[msg.sender] = balances[msg.sender].sub(_value);
        balances[_to] = balances[_to].add(_value);
        if(codeLength>0) {
            ERC223ReceivingContract receiver = ERC223ReceivingContract(_to);
            receiver.tokenFallback(msg.sender, _value, _data);
        }

        emit Transfer(msg.sender, _to, _value, _data);
        emit Transfer (msg.sender, _to, _value);

        return true;
    }
    
    /**
     * @dev Transfer the specified amount of tokens to the specified address.
     *      This function works the same with the previous one
     *      but doesn't contain `_data` param.
     *      Added due to backwards compatibility reasons.
     *
     * @param _to    Receiver address.
     * @param _value Amount of tokens that will be transferred.
     */
    function transfer(address _to, uint _value) public returns (bool success){
        uint codeLength;
        bytes memory empty;

        assembly {
            // Retrieve the size of the code on target address, this needs assembly .
            codeLength := extcodesize(_to)
        }

        balances[msg.sender] = balances[msg.sender].sub(_value);
        balances[_to] = balances[_to].add(_value);
        
        if(codeLength>0) {
            ERC223ReceivingContract receiver = ERC223ReceivingContract(_to);
            receiver.tokenFallback(msg.sender, _value, empty);
        }

        emit Transfer(msg.sender, _to, _value, empty);
        emit Transfer (msg.sender, _to, _value);

        return true;
    }

    
    /**
     * @dev Returns balance of the `_owner`.
     *
     * @param _owner   The address whose balance will be returned.
     * @return balance Balance of the `_owner`.
     */
    function balanceOf(address _owner) view public returns (uint balance) {
        return balances[_owner];
    }

    /*
    ERC 20 compatible functions
    */

    function transferFrom (address _from, address _to, uint256 _value) public returns (bool success) {
        if (allowances [_from][msg.sender] < _value) return false;
        if (balances [_from] < _value) return false;

        allowances [_from][msg.sender] = allowances [_from][msg.sender].sub(_value);

        if (_value > 0 && _from != _to) {
            balances [_from] = balances [_from].sub(_value);
            balances [_to] = balances [_to].add(_value);
            emit Transfer (_from, _to, _value);
        }

        return true;
    }

    function approve (address _spender, uint256 _value) public returns (bool success) {
        allowances [msg.sender][_spender] = _value;
        emit Approval (msg.sender, _spender, _value);

        return true;
    }

    function allowance (address _owner, address _spender) view public returns (uint256 remaining) {
        return allowances [_owner][_spender];
    }
}
