// Branches JS
var branches = [
    {id:1,name:'Main Branch',address:'123 Fitness Street, Nasr City, Cairo',phone:'+20 100 123 4567',manager:'Tarek Fawzy',status:'Active'},
    {id:2,name:'North Branch',address:'45 Alexandria Road, Heliopolis, Cairo',phone:'+20 101 234 5678',manager:'Khaled Ibrahim',status:'Active'},
    {id:3,name:'South Branch',address:'78 Maadi Street, Maadi, Cairo',phone:'+20 102 345 6789',manager:'Youssef Mohamed',status:'Active'},
    {id:4,name:'East Branch',address:'12 New Cairo Blvd, 5th Settlement, Cairo',phone:'+20 103 456 7890',manager:'Sara Mohamed',status:'Active'}
];

var currentPage=1,perPage=10,searchQuery='',editingId=null;

function filtered(){
    var q=searchQuery.toLowerCase();
    return branches.filter(b=>!q||b.name.toLowerCase().includes(q)||b.address.toLowerCase().includes(q));
}

function renderTable(){
    var data=filtered();var total=data.length;
    var pages=Math.ceil(total/perPage)||1;if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('branchesTableBody').innerHTML=slice.length?slice.map((b,i)=>`
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${b.name}</span></td>
            <td style="font-size:.82rem;color:#64748b;">${b.address}</td>
            <td>${b.phone}</td>
            <td>${b.manager}</td>
            <td><span class="gp-badge ${b.status==='Active'?'gp-badge-green':'gp-badge-red'}">${b.status}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${b.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteBranch(${b.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`).join(''):'<tr><td colspan="7" class="text-center py-4 text-muted">No branches found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
}

function openModal(id){
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Branch':'Add Branch';
    if(id){var b=branches.find(x=>x.id===id);document.getElementById('fName').value=b.name;document.getElementById('fAddress').value=b.address;document.getElementById('fPhone').value=b.phone;document.getElementById('fManager').value=b.manager;document.getElementById('fStatus').checked=b.status==='Active';}
    else{['fName','fAddress','fPhone'].forEach(f=>document.getElementById(f).value='');document.getElementById('fStatus').checked=true;}
    new bootstrap.Modal(document.getElementById('branchModal')).show();
}

function saveBranch(){
    var name=document.getElementById('fName').value.trim();
    if(!name){toastr.error('Name required');return;}
    var data={id:editingId||Date.now(),name,address:document.getElementById('fAddress').value,phone:document.getElementById('fPhone').value,manager:document.getElementById('fManager').value,status:document.getElementById('fStatus').checked?'Active':'Inactive'};
    if(editingId){var idx=branches.findIndex(x=>x.id===editingId);branches[idx]=data;toastr.success('Branch updated');}
    else{branches.push(data);toastr.success('Branch added');}
    bootstrap.Modal.getInstance(document.getElementById('branchModal')).hide();
    renderTable();
}

function deleteBranch(id){if(!confirm('Delete?'))return;branches=branches.filter(b=>b.id!==id);toastr.success('Deleted');renderTable();}
function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
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
