// Equipment JS
var equipment = [
    {id:1,name:'Treadmill',qty:5,price:15000,date:'2024-03-15',condition:'Good',location:'Cardio Zone',branch:'Main Branch',status:'Active'},
    {id:2,name:'Stationary Bike',qty:4,price:8000,date:'2024-03-15',condition:'Good',location:'Cardio Zone',branch:'Main Branch',status:'Active'},
    {id:3,name:'Elliptical Machine',qty:3,price:12000,date:'2024-06-01',condition:'New',location:'Cardio Zone',branch:'North Branch',status:'Active'},
    {id:4,name:'Rowing Machine',qty:2,price:9000,date:'2024-04-20',condition:'Needs Repair',location:'Cardio Zone',branch:'Main Branch',status:'Active'},
    {id:5,name:'Bench Press Station',qty:4,price:5000,date:'2023-11-10',condition:'Good',location:'Free Weights',branch:'Main Branch',status:'Active'},
    {id:6,name:'Cable Machine',qty:2,price:18000,date:'2024-01-15',condition:'Good',location:'Machine Area',branch:'South Branch',status:'Active'},
    {id:7,name:'Leg Press Machine',qty:2,price:14000,date:'2024-02-20',condition:'Good',location:'Machine Area',branch:'Main Branch',status:'Active'},
    {id:8,name:'Smith Machine',qty:1,price:22000,date:'2023-09-05',condition:'Needs Repair',location:'Free Weights',branch:'North Branch',status:'Active'},
    {id:9,name:'Dumbbell Set (5-50kg)',qty:2,price:25000,date:'2023-07-01',condition:'Good',location:'Free Weights',branch:'Main Branch',status:'Active'},
    {id:10,name:'Pull-up Station',qty:3,price:3000,date:'2024-05-10',condition:'New',location:'Functional Area',branch:'East Branch',status:'Active'},
    {id:11,name:'Battle Ropes',qty:4,price:1200,date:'2024-07-01',condition:'New',location:'Functional Area',branch:'Main Branch',status:'Active'},
    {id:12,name:'Kettle Bell Set',qty:2,price:4000,date:'2023-12-01',condition:'Good',location:'Free Weights',branch:'South Branch',status:'Active'},
    {id:13,name:'Boxing Bag',qty:3,price:2500,date:'2024-01-10',condition:'Needs Repair',location:'Boxing Area',branch:'Main Branch',status:'Active'},
    {id:14,name:'Spin Bike',qty:6,price:6000,date:'2024-04-01',condition:'Good',location:'Studio',branch:'North Branch',status:'Active'},
    {id:15,name:'Old Treadmill (Retired)',qty:1,price:10000,date:'2022-01-01',condition:'Broken',location:'Storage',branch:'Main Branch',status:'Inactive'}
];

var currentPage=1,perPage=8,searchQuery='',conditionF='',branchF='',editingId=null;
var conditionBadge={'New':'gp-badge-blue','Good':'gp-badge-green','Needs Repair':'gp-badge-yellow','Broken':'gp-badge-red'};

function filtered(){
    var q=searchQuery.toLowerCase();
    return equipment.filter(e=>(!q||e.name.toLowerCase().includes(q)||e.location.toLowerCase().includes(q))&&(!conditionF||e.condition===conditionF)&&(!branchF||e.branch===branchF));
}

function renderTable(){
    var data=filtered();var total=data.length;
    var pages=Math.ceil(total/perPage)||1;if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('equipTableBody').innerHTML=slice.length?slice.map((e,i)=>`
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${e.name}</span></td>
            <td><span style="font-weight:700;">${e.qty}</span></td>
            <td>${e.price.toLocaleString()} EGP</td>
            <td>${e.date}</td>
            <td><span class="gp-badge ${conditionBadge[e.condition]||'gp-badge-gray'}">${e.condition}</span></td>
            <td>${e.location}</td>
            <td>${e.branch}</td>
            <td><span class="gp-badge ${e.status==='Active'?'gp-badge-green':'gp-badge-red'}">${e.status}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${e.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteEquipment(${e.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`).join(''):'<tr><td colspan="10" class="text-center py-4 text-muted">No equipment found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
    document.getElementById('statTotal').textContent=equipment.filter(e=>e.status==='Active').length;
    document.getElementById('statRepair').textContent=equipment.filter(e=>e.condition==='Needs Repair').length;
    document.getElementById('statBroken').textContent=equipment.filter(e=>e.condition==='Broken').length;
}

function openModal(id){
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Equipment':'Add Equipment';
    if(id){var e=equipment.find(x=>x.id===id);document.getElementById('fName').value=e.name;document.getElementById('fQty').value=e.qty;document.getElementById('fPrice').value=e.price;document.getElementById('fDate').value=e.date;document.getElementById('fCondition').value=e.condition;document.getElementById('fLocation').value=e.location;document.getElementById('fBranch').value=e.branch;document.getElementById('fStatus').checked=e.status==='Active';}
    else{['fName','fPrice','fLocation'].forEach(f=>document.getElementById(f).value='');document.getElementById('fQty').value='1';document.getElementById('fDate').value=new Date().toISOString().slice(0,10);document.getElementById('fStatus').checked=true;}
    new bootstrap.Modal(document.getElementById('equipModal')).show();
}

function saveEquipment(){
    var name=document.getElementById('fName').value.trim();
    if(!name){toastr.error('Name required');return;}
    var data={id:editingId||Date.now(),name,qty:parseInt(document.getElementById('fQty').value)||1,price:parseFloat(document.getElementById('fPrice').value)||0,date:document.getElementById('fDate').value,condition:document.getElementById('fCondition').value,location:document.getElementById('fLocation').value,branch:document.getElementById('fBranch').value,status:document.getElementById('fStatus').checked?'Active':'Inactive'};
    if(editingId){var idx=equipment.findIndex(x=>x.id===editingId);equipment[idx]=data;toastr.success('Updated');}
    else{equipment.push(data);toastr.success('Added');}
    bootstrap.Modal.getInstance(document.getElementById('equipModal')).hide();
    renderTable();
}

function deleteEquipment(id){if(!confirm('Delete?'))return;equipment=equipment.filter(e=>e.id!==id);toastr.success('Deleted');renderTable();}
function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
function handleFilter(){conditionF=document.getElementById('conditionFilter').value;branchF=document.getElementById('branchFilter').value;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',renderTable);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
